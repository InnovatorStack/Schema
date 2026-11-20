import concurrent.futures
import decimal
import json
import statistics
import time


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def verify_case(case, reply, module):
    if case["Error"] is not None:
        require(reply.fault is not None, "Expected a genuine Aras SOAP fault.")
        require(case["Error"] in reply.fault, "Aras fault lost the expected error text.")
        return {"HttpStatus": reply.status, "Fault": reply.fault}
    reply.require_success()
    result_text = reply.result()
    result = json.loads(result_text, parse_float=decimal.Decimal)
    require(result["LibraryMvid"].lower() == module.lower(), "Loaded library MVID differs.")
    valid = len(case["Issues"]) == 0
    require(result["Valid"] is valid, "Validity differs.")
    require(result["HasOutput"] is valid, "Output availability differs.")
    if "State" in result:
        require(result["State"] == ("Present" if valid else "Invalid"), "Root state differs.")
    if not valid:
        require(result["Output"] is None, "Invalid validation exposed partial output.")
    require(len(result["Issues"]) == len(case["Issues"]), "Issue count differs.")
    for actual, expected in zip(result["Issues"], case["Issues"]):
        for key in ["Code", "Path", "RuleId"]:
            require(actual[key] == expected[key], key + " differs: " + str(actual[key]))
        if expected["Message"] is not None:
            require(actual["Message"] == expected["Message"], "Custom message differs.")
    if case["Output"] is not None:
        expected = json.loads(case["Output"], parse_float=decimal.Decimal)
        for key, value in expected.items():
            actual = result["Output"][key]
            numeric = type(value) in (int, decimal.Decimal)
            same_type = (type(actual) in (int, decimal.Decimal) if numeric
                         else type(actual) is type(value))
            require(same_type, "Output type differs for " + key)
            require(actual == value, "Output differs for " + key)
    if "InputUnchanged" in result:
        require(result["InputUnchanged"] is True, "The supplied IOM input was modified.")
    return {"HttpStatus": reply.status, "Result": json.loads(result_text),
            "ResultJson": result_text}


def run_cases(client, package, methods, record):
    failures = 0
    for case in package["Cases"]:
        entry = {"Name": case["Name"], "Method": methods[case["Method"]]}
        try:
            reply = client.invoke(entry["Method"], case["RequestBody"])
            entry["HttpStatus"] = reply.status
            entry["ResponseText"] = str(reply.fault) if reply.fault else reply.result()
            entry.update(verify_case(case, reply, package["LibraryMvid"]))
            entry.update(Passed=True, Milliseconds=reply.milliseconds)
            print("PASS", case["Name"], "HTTP", reply.status, flush=True)
        except Exception as error:
            failures += 1
            entry.update(Passed=False, Error=str(error))
            print("FAIL", case["Name"], str(error), flush=True)
        record["Cases"].append(entry)
    return failures


def isolation_cases(package):
    cases = {case["Name"]: case for case in package["Cases"]}
    for index in range(24):
        base = cases["JSON minimal"]
        code = "P-ISOLATION-" + str(index)
        payload = json.dumps({"item_number": code, "quantity": index + 1})
        yield dict(base, Name="Isolation " + str(index),
                   RequestBody="<payload>" + payload + "</payload>",
                   Output=json.dumps({"ItemNumber": code, "Quantity": index + 1}))
        if index % 4 == 0:
            yield dict(cases["JSON Aras error"], Name="Isolation error " + str(index))
        if index % 6 == 0:
            yield dict(cases["AML IOM valid"], Name="Isolation IOM " + str(index))


def run_isolation(client, package, methods, record):
    def run(case):
        reply = client.invoke(methods[case["Method"]], case["RequestBody"])
        verify_case(case, reply, package["LibraryMvid"])
        return {"Name": case["Name"], "Passed": True, "HttpStatus": reply.status,
                "Milliseconds": reply.milliseconds}

    failures = 0
    started = time.perf_counter()
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        pending = {pool.submit(run, case): case for case in isolation_cases(package)}
        for task in concurrent.futures.as_completed(pending):
            case = pending[task]
            try:
                entry = task.result()
            except Exception as error:
                failures += 1
                entry = {"Name": case["Name"], "Passed": False, "Error": str(error)}
            record["Isolation"].append(entry)
    record["IsolationWallMilliseconds"] = (time.perf_counter() - started) * 1000
    timings = [entry["Milliseconds"] for entry in record["Isolation"] if entry["Passed"]]
    if timings:
        record["IsolationMedianRequestMilliseconds"] = statistics.median(timings)
    print("Concurrent isolation:", len(record["Isolation"]) - failures,
          "/", len(record["Isolation"]), "passed.", flush=True)
    return failures
