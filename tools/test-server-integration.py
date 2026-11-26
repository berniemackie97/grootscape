"""Exercise the C++ codec through the unchanged .NET /play entry point."""
import base64
import hashlib
import os
from pathlib import Path
import socket
import subprocess
import sys
import tempfile
import time

peer = str(Path(sys.argv[1]).resolve())
with socket.socket() as reservation:
    reservation.bind(('127.0.0.1', 0))
    port = reservation.getsockname()[1]
env = dict(os.environ, ASPNETCORE_URLS=f'http://127.0.0.1:{port}', ASPNETCORE_ENVIRONMENT='Development')
with tempfile.TemporaryFile() as log:
    server = subprocess.Popen(['dotnet', 'run', '--no-build', '--configuration', 'Release', '--no-launch-profile', '--project', 'src/server/Grootscape.Server'], env=env, stdout=log, stderr=log)
    try:
        deadline = time.monotonic() + 30
        while True:
            try:
                connection = socket.create_connection(('127.0.0.1', port), timeout=2)
                break
            except OSError:
                if server.poll() is not None or time.monotonic() > deadline:
                    raise RuntimeError('server did not start')
                time.sleep(0.1)
        with connection:
            connection.settimeout(5)
            key = base64.b64encode(os.urandom(16)).decode()
            connection.sendall((f'GET /play HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n').encode())
            stream = connection.makefile('rb')
            status = stream.readline()
            assert b'101' in status, status
            headers = {}
            while (line := stream.readline()) != b'\r\n':
                name, value = line.decode().split(':', 1)
                headers[name.lower()] = value.strip()
            expected = base64.b64encode(hashlib.sha1((key + '258EAFA5-E914-47DA-95CA-C5AB0DC85B11').encode()).digest()).decode()
            assert headers['sec-websocket-accept'] == expected
            hello = bytes.fromhex(subprocess.check_output([peer], text=True))
            mask = os.urandom(4)
            payload = bytes(value ^ mask[i % 4] for i, value in enumerate(hello))
            connection.sendall(bytes([0x82, 0x80 | len(hello)]) + mask + payload)
            opcode, length = stream.read(2)
            assert opcode == 0x82 and length == 5, (opcode, length)
            welcome = stream.read(length)
            assert subprocess.check_output([peer, welcome.hex()], text=True).strip() == '600'
            print('C++ hello -> .NET /play -> C++ welcome: passed')
    except BaseException:
        log.seek(0)
        print(log.read().decode(), file=sys.stderr)
        raise
    finally:
        server.terminate()
        try:
            server.wait(timeout=10)
        except subprocess.TimeoutExpired:
            server.kill()
            server.wait()
