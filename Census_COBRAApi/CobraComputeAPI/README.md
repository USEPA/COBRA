# CobraComputeAPI

!Requires REDIS!
docker run -d -t -p 127.0.0.1:6379:6379 --name rediscontainer redis

# Project is intended for deployment on Azure docker containers.

# assume Dockerfile in C:\docker_deploy\cobra

# build and tag image 
docker build -t cobra:latest .
docker tag cobra abtcobraregistry.azurecr.io/cobra:latest

# login to azure and push
az acr login --name AbtCobraRegistry
docker push abtcobraregistry.azurecr.io/cobra:latest

# manually deploy through Azure UI

# deploy manually on local docker

# docker status
docker stats

# show images
docker images 

# docker remove image
docker rmi <image>

# show containers
docker ps -a

# docker remove container
docker rm <container>

# create container and run all at http://127.0.0.1:8080/api/token
docker run -d -t -p 127.0.0.1:8080:80 --name cobracontainer cobra
docker inspect --format='{{range $p, $conf := .NetworkSettings.Ports}} {{$p}} -> {{(index $conf 0).HostPort}} {{end}}' cobracontainer

docker exec -it cobracontainer /bin/bash