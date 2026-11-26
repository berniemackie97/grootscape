"""Compile canonical JSON fixtures into dependency-free C++ contract tests."""
import json
import pathlib
import sys
root = json.loads(pathlib.Path(sys.argv[1]).read_text())
assert root['protocolVersion'] == 1
lines = ['#include "grootscape/protocol.hpp"', '#include <stdexcept>', 'using namespace grootscape::protocol;', 'void test_fixtures() {']
for case in root['messages'] + root['invalid']:
    if case['direction'] != 'serverToClient' and 'message' not in case:
        continue
    data = bytes.fromhex(case['hex'])
    literal = ','.join(str(b) for b in data)
    lines.append('{ const std::array<std::uint8_t, %d> bytes{%s};' % (len(data), literal))
    if case['direction'] == 'clientToServer':
        expected = 'encode_hello(Platform::%s) == bytes' % case['message']['platform']
    else:
        if 'error' in case:
            names = {'unknownOpcode':'unknown_opcode','trailingBytes':'trailing_bytes','invalidValue':'invalid_value','unsupportedVersion':'unsupported_version'}
            expected = 'decode_server(bytes) == ServerMessage{DecodeError::%s}' % names.get(case['error'],case['error'])
        elif case['message']['type'] == 'serverWelcome':
            expected = 'decode_server(bytes) == ServerMessage{Welcome{%d}}' % case['message']['tickDurationMs']
        else:
            expected = 'decode_server(bytes) == ServerMessage{Reject{RejectReason::%s}}' % {'unsupportedVersion':'unsupported_version','malformed':'malformed'}[case['message']['reason']]
    lines.append('if (!(%s)) throw std::runtime_error("%s"); }' % (expected, case['name']))
lines.append('}')
pathlib.Path(sys.argv[2]).write_text('\n'.join(lines))
