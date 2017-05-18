# Docker Support JavaScriptServices

Using Visual Studio 2017 you can right click each project and Add Docker Support. Visual Studio will create a Dockerfile for the project and register it in the docker-compose section of your solution.
The Dockerfile created by Visual Studio looks like this:
```
FROM microsoft/aspnetcore:1.1
ARG source
WORKDIR /app
EXPOSE 80
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "MusicStore.dll"]
```

The problem is that JavaScriptServices needs NodeJS to run, which is not available on the microsoft/aspnetcore:1.1 image. We need to install it before starting the application.
Change the Dockerfile like this:
```
FROM microsoft/aspnetcore:1.1
RUN apt-get update
RUN apt-get install curl
RUN curl -sL https://deb.nodesource.com/setup_6.x | bash
RUN apt-get install -y build-essential nodejs
ARG source
WORKDIR /app
EXPOSE 80
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "MusicStore.dll"]
```

Docker support was added to all sample projects. The React MusicStore is not work properly due to a webpack error.

## Running with Docker
To run the application inside Docker, make sure that docker-compose is the Start-up project on your solution. Then, simply press F5 which will run all configured samples in a docker container.
To browse go to the command line and type:
```
docker ps
CONTAINER ID        IMAGE                      COMMAND               CREATED             STATUS              PORTS                   NAMES
8da5f8ec45fe        reactmusicstore:dev        "tail -f /dev/null"   12 minutes ago      Up 12 minutes       0.0.0.0:32779->80/tcp   dockercompose3094088439_reactmusicstore_1
16593eafd06a        reactgrid:dev              "tail -f /dev/null"   12 minutes ago      Up 12 minutes       0.0.0.0:32780->80/tcp   dockercompose3094088439_reactgrid_1
7fda780decb0        ngmusicstore:dev           "tail -f /dev/null"   12 minutes ago      Up 12 minutes       0.0.0.0:32778->80/tcp   dockercompose3094088439_ngmusicstore_1
7c0d49413df6        nodeservicesexamples:dev   "tail -f /dev/null"   12 minutes ago      Up 12 minutes       0.0.0.0:32777->80/tcp   dockercompose3094088439_nodeservicesexamples_1
0f15ba2c085c        reactgrid:dev              "tail -f /dev/null"   About an hour ago   Up About an hour    0.0.0.0:32772->80/tcp   dockercompose4052802262_reactgrid_1
```

If you open your browser on localhost:32778 you will see the Angular Music Store Sample. I've added in page footer information about the hosting environment.
Under docker (at least for Windows) the footer should look like:
```
Running on 7fda780decb0 (Linux 4.9.12-moby #1 SMP Tue Feb 28 12:11:36 UTC 2017)
```
