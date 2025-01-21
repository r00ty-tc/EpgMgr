FROM debian:bookworm
ARG USER_ID
ARG GROUP_ID
RUN apt update && apt upgrade -y && apt install -y build-essential git curl gh wget 7zip
RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt-get update && apt-get install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0
RUN groupadd -g ${GROUP_ID} jenkins
RUN useradd jenkins -u ${USER_ID} -g ${GROUP_ID} --shell /bin/bash --create-home
