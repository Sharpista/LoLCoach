#!/usr/bin/env python3
"""Proxy HTTPS local (equivalente ao Kong do Supabase): recebe em /rest/v1/... e
encaminha para o PostgREST de fixture na raiz. Necessario porque SupabaseStore
exige URL https:// — permite rodar o cliente real sem alterar codigo dele.
"""
from __future__ import annotations

import http.server
import ssl
import urllib.error
import urllib.request

UPSTREAM = "http://127.0.0.1:3300"
PORT = 8443
CERT = "/tmp/lol61/cert.pem"
KEY = "/tmp/lol61/key.pem"
FORWARD_HEADERS = ("apikey", "Authorization", "Content-Type", "Accept", "Prefer", "Range")


class Handler(http.server.BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def _forward(self) -> None:
        length = int(self.headers.get("Content-Length") or 0)
        body = self.rfile.read(length) if length else None
        path = self.path
        if path.startswith("/rest/v1"):
            path = path[len("/rest/v1"):] or "/"
        request = urllib.request.Request(UPSTREAM + path, data=body, method=self.command)
        for header in FORWARD_HEADERS:
            value = self.headers.get(header)
            if value:
                request.add_header(header, value)
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                data, status = response.read(), response.status
        except urllib.error.HTTPError as exc:
            data, status = exc.read(), exc.code
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    do_GET = _forward
    do_POST = _forward
    do_PATCH = _forward
    do_DELETE = _forward

    def log_message(self, fmt: str, *args: object) -> None:  # silencioso
        return


def main() -> None:
    server = http.server.ThreadingHTTPServer(("127.0.0.1", PORT), Handler)
    context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
    context.load_cert_chain(CERT, KEY)
    server.socket = context.wrap_socket(server.socket, server_side=True)
    print(f"proxy https em https://127.0.0.1:{PORT}/rest/v1 -> {UPSTREAM}")
    server.serve_forever()


if __name__ == "__main__":
    main()
