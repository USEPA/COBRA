ideal steps:

recommend setting docker to 4+GB as cobra api uses 2GB by itself

1) start iamge/container building, start and detach, will take some time, check by accessing minio in step 2.
docker-compose up -d   

2). this step only required to load initial files into s3/minio
log into minio (for local development look at credentials in docker-compose.yaml)
http://localhost:9000
!!!!!!create a bucket "cobra" !!!!! - icon lower right
upload the files in minio_data_for_manual_upload into the new cobra bucket, drag and drop works well

3) trigger API initialization
browse to http://localhost:8080/api/version - this can take up to 15 minutes
monitor for cpu drop using docker stats
http://127.0.0.1:8080/api/log should then show something like: 
initializing configurationsinitializing miniobeginning initialization data/entering load datagarbage collectioninstantiating managerinit done True

API should now work as in production
http://127.0.0.1:8080/api/token
http://127.0.0.1:8080/api/datadictionary?which=states
http://127.0.0.1:8080/api/datadictionary?which=tiers
etc


4) take docker containers down, maintains docker volumes
docker-compose down

################################################################################################

#show stats
docker stats

# pause and unpause containers
docker-compose pause
docker-compose unpause

#take down single container
docker-compose rm -s -v cobranetcore

#start up a single docker container
docker-compose up -d cobranetcore

#look at what is running 
docker ps -a

# in case of weird problems, rebuild images
docker-compose build cobranetcore 
docker-compose up --force-recreate

#take down removing volumes/data. basically reset all
docker-compose down -v


