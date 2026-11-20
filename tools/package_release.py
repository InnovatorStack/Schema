"""Package only the owned assembly and documentation for one framework."""

import argparse
import hashlib
import json
import re
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo


FRAMEWORKS = ("net6.0", "net8.0")
FILES = {"InnovatorSchema.dll", "InnovatorSchema.xml", "README.md", "manifest.json", "LICENSE"}
VERSION = re.compile(r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)"
                     r"(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?")


def validate_version(value):
    match = VERSION.fullmatch(value)
    if not match or len(value) > 100:
        raise ValueError("Use a SemVer version without build metadata.")
    for part in (match.group(4) or "").split("."):
        if part.isdigit() and len(part) > 1 and part.startswith("0"):
            raise ValueError("Numeric prerelease identifiers cannot start with zero.")
    return value


def digest(data):
    return hashlib.sha256(data).hexdigest()


def archive_name(version, framework):
    return f"Schema-{version}-{framework}.zip"


def read_build(directory, version, framework):
    dependencies = json.loads((directory / "InnovatorSchema.deps.json").read_text())
    expected_target = f".NETCoreApp,Version=v{framework[3:]}"
    if dependencies["runtimeTarget"]["name"] != expected_target:
        raise ValueError("Build target framework does not match the selected asset.")
    libraries = dependencies["libraries"]
    if f"InnovatorSchema/{version}" not in libraries:
        raise ValueError("Build version does not match the selected release version.")
    if set(libraries) != {f"InnovatorSchema/{version}"}:
        raise ValueError("The library must have framework dependencies only.")
    return {name: (directory / name).read_bytes()
            for name in ("InnovatorSchema.dll", "InnovatorSchema.xml")}


def package(args):
    version = validate_version(args.version)
    if not re.fullmatch(r"[0-9a-f]{40}", args.commit):
        raise ValueError("A complete lowercase Git commit SHA is required.")
    files = read_build(args.input, version, args.framework)
    manifest = {
        "version": version, "commit": args.commit, "framework": args.framework,
        "assembly": "InnovatorSchema.dll",
        "assemblySha256": digest(files["InnovatorSchema.dll"]),
    }
    files["LICENSE"] = (Path(__file__).resolve().parents[1] / "LICENSE").read_bytes()
    files["manifest.json"] = (json.dumps(manifest, indent=2) + "\n").encode()
    files["README.md"] = (
        f"# Schema {version}\n\nTarget: {args.framework}.\n\n"
        "Copy InnovatorSchema.dll into Server/bin and add its reference to method-config.xml. "
        "Add the InnovatorSchema namespace and Validator alias to the CSharp Method template.\n\n"
        "[Installation](https://innovatorstack.github.io/Schema/guides/installation.html)\n\n"
        "License: MIT. See LICENSE in this archive.\n"
    ).encode()
    args.output.mkdir(parents=True, exist_ok=True)
    path = args.output / archive_name(version, args.framework)
    with path.open("xb") as output, ZipFile(output, "w", ZIP_DEFLATED) as archive:
        for name, content in sorted(files.items()):
            entry = ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            entry.compress_type = ZIP_DEFLATED
            entry.external_attr = 0o100644 << 16
            archive.writestr(entry, content)
    verify_archive(path, manifest)
    print(path)


def verify_archive(path, expected):
    with ZipFile(path) as archive:
        if set(archive.namelist()) != FILES or len(archive.namelist()) != len(FILES):
            raise ValueError(f"Unexpected archive contents: {path}")
        manifest = json.loads(archive.read("manifest.json"))
        if manifest != expected:
            raise ValueError(f"Archive manifest mismatch: {path}")
        if digest(archive.read("InnovatorSchema.dll")) != manifest["assemblySha256"]:
            raise ValueError(f"Archive assembly hash mismatch: {path}")


def checksums(args):
    version = validate_version(args.version)
    expected = {archive_name(version, framework) for framework in FRAMEWORKS}
    actual = {path.name for path in args.output.glob("*.zip")}
    if actual != expected:
        raise ValueError(f"Expected the two framework assets, found: {sorted(actual)}")
    lines = []
    for framework in FRAMEWORKS:
        path = args.output / archive_name(version, framework)
        with ZipFile(path) as archive:
            manifest = json.loads(archive.read("manifest.json"))
        if (manifest["version"], manifest["commit"], manifest["framework"]) != (
                version, args.commit, framework):
            raise ValueError(f"Wrong build identity: {path}")
        verify_archive(path, manifest)
        lines.append(f"{digest(path.read_bytes())}  {path.name}\n")
    (args.output / "SHA256SUMS.txt").write_text("".join(sorted(lines)))
    print(f"Verified {len(lines)} archives and wrote SHA256SUMS.txt.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--validate-version")
    parser.add_argument("--checksums", action="store_true")
    parser.add_argument("--version")
    parser.add_argument("--commit")
    parser.add_argument("--framework", choices=FRAMEWORKS)
    parser.add_argument("--input", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    if args.validate_version:
        print(validate_version(args.validate_version))
        return
    required = ("version", "commit", "output")
    if not args.checksums:
        required += ("framework", "input")
    for name in required:
        if getattr(args, name) is None:
            parser.error(f"--{name} is required")
    (checksums if args.checksums else package)(args)


if __name__ == "__main__":
    main()
