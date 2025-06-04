FROM mcr.microsoft.com/devcontainers/base:ubuntu-24.04

SHELL ["/bin/bash", "-c"]
RUN apt-get update && apt-get upgrade -y && \
    apt-get install -y software-properties-common &&\
    add-apt-repository ppa:dotnet/backports && \
    source /etc/os-release && \
    wget https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-6.0
