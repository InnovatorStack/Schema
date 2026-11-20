import hashlib
import json
import time
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
from dataclasses import dataclass


def local_name(tag):
    return tag.rsplit("}", 1)[-1]


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        return None


@dataclass
class Reply:
    status: int
    root: ET.Element
    milliseconds: float

    @property
    def fault(self):
        for node in self.root.iter():
            if local_name(node.tag) == "Fault":
                for child in node:
                    if local_name(child.tag) == "faultstring":
                        return "".join(child.itertext())
                return "Aras returned a SOAP fault without a faultstring."
        return None

    def require_success(self):
        if self.fault is not None:
            raise RuntimeError(self.fault)
        if not 200 <= self.status < 300:
            raise RuntimeError("Aras returned HTTP " + str(self.status))
        return self

    def items(self, item_type):
        return [node for node in self.root.iter()
                if local_name(node.tag) == "Item" and node.get("type") == item_type]

    def result(self):
        for node in self.root.iter():
            if local_name(node.tag) == "Result":
                return "".join(node.itertext())
        raise RuntimeError("Aras response contains no Result element.")


class Aras:
    def __init__(self, url, database, username):
        self.url = url.rstrip("/") + "/"
        self.database = database
        self.username = username
        self.token = None
        self.opener = urllib.request.build_opener(NoRedirect)

    def request(self, url, data=None, headers=None):
        request = urllib.request.Request(url, data=data, headers=headers or {})
        try:
            response = self.opener.open(request, timeout=60)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            body = response.read(8 * 1024 * 1024 + 1)
            if len(body) > 8 * 1024 * 1024:
                raise RuntimeError("Aras response exceeds 8 MiB.")
            return response.code, body

    def login(self, password, client_id="IOMApp"):
        status, body = self.request(
            self.url + "OAuthServer/.well-known/openid-configuration")
        if status != 200:
            raise RuntimeError("OAuth discovery returned HTTP " + str(status))
        endpoint = json.loads(body)["token_endpoint"]
        if urllib.parse.urlsplit(endpoint)[:2] != urllib.parse.urlsplit(self.url)[:2]:
            raise RuntimeError("The token endpoint is outside the configured server origin.")
        fields = {
            "grant_type": "password", "client_id": client_id, "scope": "Innovator",
            "database": self.database, "username": self.username
        }
        for algorithm in ("md5", "sha256"):
            fields["password"] = hashlib.new(algorithm, password.encode("utf-8"),
                                             usedforsecurity=False).hexdigest()
            data = urllib.parse.urlencode(fields).encode("utf-8")
            status, body = self.request(endpoint, data, {
                "Content-Type": "application/x-www-form-urlencoded", "Accept": "application/json"
            })
            result = json.loads(body)
            if status == 200 and result.get("access_token"):
                self.token = result["access_token"]
                return result.get("expires_in")
            if algorithm != "md5" or result.get("error") != "incompatible_hash_use_sha256":
                break
        raise RuntimeError("OAuth login failed: HTTP " + str(status) + " / " +
                           str(result.get("error", "missing access token")))

    def aml(self, items):
        aml = ET.Element("AML")
        aml.extend(items)
        return self.call("ApplyAML", aml)

    def call(self, action, payload):
        if self.token is None:
            raise RuntimeError("Authenticate before issuing AML.")
        envelope = ET.Element("{http://schemas.xmlsoap.org/soap/envelope/}Envelope")
        body = ET.SubElement(envelope, "{http://schemas.xmlsoap.org/soap/envelope/}Body")
        body.append(payload)
        started = time.perf_counter()
        status, response = self.request(
            self.url + "Server/InnovatorServer.aspx", ET.tostring(envelope, encoding="utf-8"),
            {"Authorization": "Bearer " + self.token, "SOAPAction": action,
             "DATABASE": self.database, "Content-Type": "text/xml; charset=utf-8"})
        elapsed = (time.perf_counter() - started) * 1000
        try:
            root = ET.fromstring(response)
        except ET.ParseError as error:
            raise RuntimeError("Aras returned non-XML data, HTTP " + str(status)) from error
        return Reply(status, root, elapsed)

    def query(self, item_type, properties=None, select="id", item_id=None):
        item = ET.Element("Item", {"type": item_type, "action": "get", "select": select})
        if item_id is not None:
            item.set("id", item_id)
        for name, value in (properties or {}).items():
            ET.SubElement(item, name).text = value
        return self.aml([item])

    def invoke(self, name, body):
        item = ET.Element("Item", {"type": "Method", "action": name})
        for child in ET.fromstring("<request>" + body + "</request>"):
            item.append(child)
        return self.call("ApplyMethod", item)
