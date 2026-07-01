FROM ubuntu:26.04

RUN apt --yes update

RUN apt --yes install --no-install-recommends git

# We need to install `ca-certificates`, otherwise we get these errors in the CI:
# Unable to load the service index for source https://api.nuget.org/v3/index.json.
# The SSL connection could not be established, see inner exception.
# The remote certificate is invalid because of errors in the certificate chain: UntrustedRoot
RUN apt --yes install --no-install-recommends ca-certificates

RUN apt --yes install --no-install-recommends dotnet-sdk-10.0

# commitlint depends on npm and yarn
RUN apt --yes install --no-install-recommends npm
RUN npm install --global yarn

# cleanup apt cache to reduce img size & avoid unneeded files in the final layer
RUN rm -rf /var/lib/apt/lists/*

CMD ["bash"]
