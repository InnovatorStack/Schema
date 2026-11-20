import argparse
import datetime
import getpass
import hashlib
import json
import os
from pathlib import Path
import re
import sys
import uuid
import xml.etree.ElementTree as ET

from aras_http import Aras, local_name
from payload_checks import run_cases, run_isolation


def save(path, value):
    temporary = path.with_suffix(".pending")
    temporary.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    os.chmod(temporary, 0o600)
    temporary.replace(path)


def identity(client):
    users = client.query("User", {"login_name": client.username},
                         select="id,login_name").require_success().items("User")
    if len(users) != 1:
        raise RuntimeError("Expected exactly one authenticated User.")
    aliases = client.query("Alias", {"source_id": users[0].get("id")},
                           select="id,related_id").require_success().items("Alias")
    identities = []
    for alias in aliases:
        for child in alias:
            if local_name(child.tag) != "related_id":
                continue
            identities.extend(node.get("id") for node in child
                              if local_name(node.tag) == "Item")
            if not list(child) and child.text:
                identities.append(child.text)
    if len(identities) != 1 or re.fullmatch("[0-9A-Fa-f]{32}", identities[0]) is None:
        raise RuntimeError("Expected one explicit execution identity for the test user.")
    return identities[0]


def deploy(client, package, journal, path):
    for source in package["Methods"]:
        name = "InnovatorSchema_" + journal["RunId"] + "_" + source["Name"][8:]
        if len(name) > 32:
            raise RuntimeError("The generated Method name exceeds Aras's 32-character limit.")
        item_id = uuid.uuid4().hex.upper()
        item = ET.Element("Item", {"type": "Method", "action": "add", "id": item_id})
        properties = {"name": name, "method_type": "C#", "method_code": source["Code"],
                      "execution_allowed_to": journal["ExecutionIdentity"]}
        for key, value in properties.items():
            ET.SubElement(item, key).text = value
        entry = {"Id": item_id, "Name": name, "Source": source["Name"], "State": "Intent",
                 "CodeSha256": hashlib.sha256(source["Code"].encode("utf-8")).hexdigest()}
        journal["Methods"].append(entry)
        save(path, journal)
        response = client.aml([item]).require_success()
        created = response.items("Method")
        if len(created) != 1 or created[0].get("id") != item_id:
            raise RuntimeError("Method creation returned an unexpected id; inspect the journal.")
        entry["State"] = "Created"
        save(path, journal)
        print("Created", name, item_id, flush=True)
    return {entry["Source"]: entry["Name"] for entry in journal["Methods"]}


def reuse(client, package, path):
    journal = json.loads(path.read_text(encoding="utf-8"))
    if (journal["Url"].rstrip("/") != client.url.rstrip("/") or
            journal["Database"] != client.database or
            journal["LibrarySha256"] != package["LibrarySha256"]):
        raise RuntimeError("The deployment does not match this connection and library build.")
    expected = {source["Name"]: hashlib.sha256(source["Code"].encode("utf-8")).hexdigest()
                for source in package["Methods"]}
    if len(journal["Methods"]) != len(expected):
        raise RuntimeError("The deployment does not contain the expected Method set.")
    for entry in journal["Methods"]:
        if entry["State"] != "Created" or entry["CodeSha256"] != expected[entry["Source"]]:
            raise RuntimeError("Inspect the existing Method deployment before reusing it.")
        reply = client.query("Method", item_id=entry["Id"],
                             select="id,name,method_code").require_success()
        items = reply.items("Method")
        if len(items) != 1:
            raise RuntimeError("A deployed Method is missing or ambiguous.")
        properties = {local_name(child.tag): child.text or "" for child in items[0]}
        actual_hash = hashlib.sha256(properties["method_code"].encode("utf-8")).hexdigest()
        if properties["name"] != entry["Name"] or actual_hash != entry["CodeSha256"]:
            raise RuntimeError("A deployed Method has changed; inspect it before invoking it.")
    return journal


def remove_methods(client, package, journal, path):
    reuse(client, package, path)
    journal["Cleanup"] = "InProgress"
    save(path, journal)
    for entry in journal["Methods"]:
        entry["State"] = "DeleteIntent"
        save(path, journal)
        item = ET.Element("Item", {"type": "Method", "action": "delete", "id": entry["Id"]})
        client.aml([item]).require_success()
        entry["State"] = "Deleted"
        save(path, journal)
    journal["Cleanup"] = "Completed"
    save(path, journal)
    print("Removed", len(journal["Methods"]), "temporary diagnostic Methods.", flush=True)


def main():
    parser = argparse.ArgumentParser(description="Deploy and invoke isolated Aras test Methods.")
    parser.add_argument("--url", required=True)
    parser.add_argument("--database", required=True)
    parser.add_argument("--username", required=True)
    parser.add_argument("--package", type=Path, required=True)
    parser.add_argument("--work-dir", type=Path, required=True)
    parser.add_argument("--reuse-deployment", type=Path)
    parser.add_argument("--password-env", help="Name of the variable containing the test password.")
    parser.add_argument("--keep-methods", action="store_true",
                        help="Keep newly created diagnostic Methods after a passing run.")
    args = parser.parse_args()
    args.work_dir.mkdir(mode=0o700, parents=True, exist_ok=True)
    journal_path = args.work_dir / "deployment.json"
    if journal_path.exists():
        raise RuntimeError("A deployment journal already exists; inspect it before another write.")
    package = json.loads(args.package.read_text(encoding="utf-8"))
    client = Aras(args.url, args.database, args.username)
    password = (os.environ[args.password_env] if args.password_env
                else getpass.getpass("Aras test password: "))
    lifetime = client.login(password)
    del password
    print("Authenticated. Token lifetime:", lifetime, "seconds.", flush=True)
    if args.reuse_deployment is None:
        journal = {"RunId": uuid.uuid4().hex[:8].upper(), "Url": args.url,
                   "Database": args.database, "UserName": args.username,
                   "StartedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                   "ExecutionIdentity": identity(client), "LibraryMvid": package["LibraryMvid"],
                   "LibrarySha256": package["LibrarySha256"], "Methods": []}
        save(journal_path, journal)
        methods = deploy(client, package, journal, journal_path)
    else:
        journal = reuse(client, package, args.reuse_deployment)
        save(journal_path, journal)
        methods = {entry["Source"]: entry["Name"] for entry in journal["Methods"]}
    report = {"LibraryMvid": package["LibraryMvid"], "Cases": [], "Isolation": []}
    failures = run_cases(client, package, methods, report)
    save(args.work_dir / "results.json", report)
    if failures == 0:
        failures += run_isolation(client, package, methods, report)
        save(args.work_dir / "results.json", report)
    journal["State"] = "Passed" if failures == 0 else "Failed"
    save(journal_path, journal)
    if failures == 0 and args.reuse_deployment is None and not args.keep_methods:
        remove_methods(client, package, journal, journal_path)
    print("Payload suite:", len(report["Cases"]) - sum(not x["Passed"] for x in report["Cases"]),
          "/", len(report["Cases"]), "passed.", flush=True)
    return 0 if failures == 0 else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as error:
        print(type(error).__name__ + ": " + str(error), file=sys.stderr)
        sys.exit(1)
