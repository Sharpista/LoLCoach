#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p artifacts
# pipefail preserves failure status while keeping reproducible evidence.
dotnet build LoLCoach.slnx -v minimal | tee artifacts/build.log
dotnet test LoLCoach.slnx --no-build --logger 'trx;LogFileName=backend-tests.trx'   --results-directory artifacts/test-results -v minimal | tee artifacts/test.log
dotnet format LoLCoach.slnx --verify-no-changes --no-restore
dotnet list LoLCoach.slnx package --vulnerable --include-transitive | tee artifacts/packages-audit.log
python3 scripts/smoke_host.py
