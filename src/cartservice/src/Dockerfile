# Copyright The OpenTelemetry Authors
# SPDX-License-Identifier: Apache-2.0
# Copyright 2021 Google LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#      http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# https://mcr.microsoft.com/v2/dotnet/sdk/tags/list
FROM mcr.microsoft.com/dotnet/sdk:6.0.412 AS builder

WORKDIR /usr/src/app/

COPY ./src/cartservice/ ./
COPY ./pb/ ./src/protos/
COPY --from=pyroscope/pyroscope-dotnet:0.8.14-musl /Pyroscope.Profiler.Native.so ./Pyroscope.Profiler.Native.so
COPY --from=pyroscope/pyroscope-dotnet:0.8.14-musl /Pyroscope.Linux.ApiWrapper.x64.so ./Pyroscope.Linux.ApiWrapper.x64.so
RUN dotnet add ./src/cartservice.csproj package Pyroscope



RUN \
  RUNTIME_IDENTIIFER=linux-musl-x64; \
  if [ "$(uname -m)" = "aarch64" ]; then RUNTIME_IDENTIIFER=linux-musl-arm64; fi; \
  dotnet restore ./src/cartservice.csproj -v d -r $RUNTIME_IDENTIIFER;


RUN \
  RUNTIME_IDENTIIFER=linux-musl-x64; \
  if [ "$(uname -m)" = "aarch64" ]; then RUNTIME_IDENTIIFER=linux-musl-arm64; fi; \
  dotnet publish ./src/cartservice.csproj -v d -p:PublishSingleFile=true -r $RUNTIME_IDENTIIFER --self-contained true -p:PublishTrimmed=False -p:TrimMode=Link -c Release -o /cartservice --no-restore

# -----------------------------------------------------------------------------

# https://mcr.microsoft.com/v2/dotnet/runtime-deps/tags/list
FROM mcr.microsoft.com/dotnet/runtime-deps:6.0.15-alpine3.16

WORKDIR /usr/src/app/
COPY --from=builder /cartservice/ ./
COPY --from=pyroscope/pyroscope-dotnet:0.8.14-musl /Pyroscope.Profiler.Native.so ./Pyroscope.Profiler.Native.so
COPY --from=pyroscope/pyroscope-dotnet:0.8.14-musl /Pyroscope.Linux.ApiWrapper.x64.so ./Pyroscope.Linux.ApiWrapper.x64.so

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={BD1A650D-AC5D-4896-B64F-D6FA25D6B26A}
ENV CORECLR_PROFILER_PATH=/usr/src/app/Pyroscope.Profiler.Native.so
ENV LD_PRELOAD=/usr/src/app/Pyroscope.Linux.ApiWrapper.x64.so

ENV PYROSCOPE_APPLICATION_NAME=cartservice
ENV PYROSCOPE_SERVER_ADDRESS=https://grafana-pyroscope-ingest.demo.perfana.cloud
ENV PYROSCOPE_LOG_LEVEL=debug
ENV PYROSCOPE_PROFILING_ENABLED=1
ENV PYROSCOPE_PROFILING_ALLOCATION_ENABLED=true
ENV PYROSCOPE_PROFILING_CONTENTION_ENABLED=true
ENV PYROSCOPE_PROFILING_EXCEPTION_ENABLED=true


EXPOSE ${CART_SERVICE_PORT}
ENTRYPOINT [ "./cartservice" ]
