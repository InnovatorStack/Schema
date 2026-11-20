"""Fail on vulnerable packages reported by dotnet list package --format json."""

import json
import subprocess
import sys


def main():
    command = ["dotnet", "list", "src/InnovatorSchema/InnovatorSchema.csproj", "package",
               "--vulnerable", "--include-transitive", "--format", "json"]
    result = subprocess.run(command, check=True, capture_output=True, text=True)
    report = json.loads(result.stdout)
    findings = []
    for project in report.get("projects", []):
        for framework in project.get("frameworks", []):
            for group in ("topLevelPackages", "transitivePackages"):
                for package in framework.get(group, []):
                    for vulnerability in package.get("vulnerabilities", []):
                        findings.append(f"{framework['framework']}: {package['id']} "
                                        f"{package['resolvedVersion']}: "
                                        f"{vulnerability['severity']} {vulnerability['advisoryurl']}")
    if report.get("problems"):
        raise RuntimeError(report["problems"])
    if findings:
        print("\n".join(findings), file=sys.stderr)
        return 1
    print("No vulnerable packages reported in the runtime restore graph.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
