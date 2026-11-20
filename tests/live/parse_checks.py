"""Invoke runtime parsing through real Aras Methods and remove the test Methods on success."""

import argparse
import getpass
import hashlib
import json
import os
from pathlib import Path
import uuid
import xml.etree.ElementTree as ET

from aras_http import Aras, local_name
from run import identity, save


def create(client, name, code, permission, journal, path):
    item_id = uuid.uuid4().hex.upper()
    entry = {'Id': item_id, 'Name': name, 'CodeSha256': hashlib.sha256(code.encode()).hexdigest(),
             'State': 'Intent'}
    journal['Methods'].append(entry)
    save(path, journal)
    node = ET.Element('Item', {'type': 'Method', 'action': 'add', 'id': item_id})
    for key, value in {'name': name, 'method_type': 'C#', 'method_code': code,
                       'execution_allowed_to': permission}.items():
        ET.SubElement(node, key).text = value
    result = client.aml([node]).require_success().items('Method')
    assert len(result) == 1 and result[0].get('id') == item_id
    entry['State'] = 'Created'
    save(path, journal)
    return name


def invoke(client, name, properties, kind='Method', children=None):
    node = ET.Element('Item', {'type': kind, 'action': name})
    for key, value in properties.items():
        ET.SubElement(node, key).text = value
    if children is not None:
        node.append(ET.fromstring(children))
    return client.call('ApplyMethod', node)


def checks(client, methods, module, results):
    rows = [{'item_number': 'P-100', 'weight': '2.5', 'extra': 123},
            {'item_number': 'P-200'}]
    aml = "<AML><Item type='Part'><item_number>P-100</item_number><weight>2.5</weight>" + \
          "<extra>allowed</extra></Item><Item type='Part'><item_number>P-200</item_number>" + \
          "</Item></AML>"
    cases = [('json', json.dumps(rows)), ('aml', aml), ('iom', aml),
             ('xml', aml.replace('<AML>', '<rows>').replace('</AML>', '</rows>'))]
    for format, payload in cases:
        for call in ('safe', 'parse'):
            result = json.loads(invoke(client, methods['values'],
                {'payload': payload, 'format': format, 'call': call}).require_success().result())
            assert result['LibraryMvid'].lower() == module.lower()
            assert result['Valid'] and result['InputUnchanged']
            assert result['Data'] == [{'item_number': 'P-100', 'weight': 2.5},
                                        {'item_number': 'P-200'}], result
            results.append({'Name': format + ' ' + call, 'Passed': True})
    invalid = '[{"item_number":" ","weight":"-1"},{}]'
    for call in ('safe', 'parse'):
        result = json.loads(invoke(client, methods['values'],
            {'payload': invalid, 'call': call}).require_success().result())
        assert [x['Path'] for x in result['Issues']] == \
            ['$[0].item_number', '$[0].weight', '$[1].item_number'], result
        assert 'Problems:' in result['Summary']
        if call == 'parse':
            assert result['ExceptionType'] == 'InnovatorSchema.ValidationException'
        else:
            assert not result['Valid'] and result['Data'] is None
        results.append({'Name': 'errors ' + call, 'Passed': True})
    for call in ('safe', 'parse'):
        result = json.loads(invoke(client, methods['values'],
            {'payload': '[{"item_number":"P-100"}]', 'callback': '1', 'call': call})
            .require_success().result())
        assert result['ExceptionType'] == 'System.FormatException', result
        results.append({'Name': 'callback ' + call, 'Passed': True})
    for call in ('safe', 'parse'):
        result = json.loads(invoke(client, methods['values'],
            {'payload': '[', 'call': call}).require_success().result())
        assert result['Issues'][0]['Code'] == 'MalformedJson', result
        if call == 'parse':
            assert result['ExceptionType'] == 'InnovatorSchema.ValidationException'
        results.append({'Name': 'malformed ' + call, 'Passed': True})
    for payload, expected in (('[]', []), ('[{"item_number":"P-1","weight":null}]',
                                           [{'item_number': 'P-1', 'weight': None}])):
        result = json.loads(invoke(client, methods['values'], {'payload': payload})
                            .require_success().result())
        assert result['Data'] == expected, result
        results.append({'Name': 'presence ' + payload, 'Passed': True})
    reply = invoke(client, methods['part'], {'item_number': 'P-100', 'extra': 'allowed'}, 'Part')
    assert reply.require_success().result() == 'P-100'
    results.append({'Name': 'consumer Part', 'Passed': True})
    reply = invoke(client, methods['parts'], {}, children=
        '<parts><Item type="Part"><item_number>P-200</item_number></Item>' +
        '<Item type="Part"><item_number>P-300</item_number></Item></parts>')
    assert reply.require_success().result() == 'P-200'
    results.append({'Name': 'consumer collection', 'Passed': True})
    reply = invoke(client, methods['parts'], {}, children=
        '<parts><Item type="Part"/><Item type="Part"/></parts>')
    assert reply.fault and '$[0].item_number' in reply.fault and '$[1].item_number' in reply.fault
    results.append({'Name': 'consumer collected errors', 'Passed': True})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--url', required=True)
    parser.add_argument('--database', required=True)
    parser.add_argument('--username', required=True)
    parser.add_argument('--module', required=True)
    parser.add_argument('--work-dir', type=Path, required=True)
    parser.add_argument('--password-env')
    args = parser.parse_args()
    args.work_dir.mkdir(parents=True, exist_ok=False)
    path = args.work_dir / 'deployment.json'
    journal = {'Methods': [], 'State': 'Prepared'}
    client = Aras(args.url, args.database, args.username)
    password = os.environ[args.password_env] if args.password_env else getpass.getpass('Aras password: ')
    client.login(password)
    del password
    permission = identity(client)
    root = Path(__file__).resolve().parents[2]
    suffix = uuid.uuid4().hex[:8]
    methods = {}
    sources = {'values': root / 'tests/ServerMethods/ParseValues.cs',
               'part': root / 'examples/ParsePartMethod.cs',
               'parts': root / 'examples/ParsePartsMethod.cs'}
    for key, source in sources.items():
        methods[key] = create(client, 'Schema_' + key + '_' + suffix, source.read_text(),
                              permission, journal, path)
    results = []
    checks(client, methods, args.module, results)
    save(args.work_dir / 'results.json', results)
    for entry in journal['Methods']:
        found = client.query('Method', item_id=entry['Id'], select='id,name,method_code') \
            .require_success().items('Method')
        assert len(found) == 1
        properties = {local_name(n.tag): n.text or '' for n in found[0]}
        assert properties['name'] == entry['Name']
        assert hashlib.sha256(properties['method_code'].encode()).hexdigest() == entry['CodeSha256']
        entry['State'] = 'DeleteIntent'
        save(path, journal)
        client.aml([ET.Element('Item', {'type': 'Method', 'action': 'delete', 'id': entry['Id']})]) \
            .require_success()
        entry['State'] = 'Deleted'
        save(path, journal)
    journal['State'] = 'Completed'
    save(path, journal)
    print('PASS', len(results), 'live parsing checks; temporary Methods removed.')


if __name__ == '__main__':
    main()
