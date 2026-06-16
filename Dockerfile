FROM ubuntu:26.04

RUN apt update && \
    apt install --yes --no-install-recommends \
        sudo \
        git \
        ca-certificates \
        dotnet-sdk-10.0 \
        npm \
        curl && \
    npm install --global yarn && \
    rm -rf /var/lib/apt/lists/*

CMD ["bash"]
