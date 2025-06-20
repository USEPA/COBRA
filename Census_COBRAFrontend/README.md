# README #

This application was built with node version 16.19.0

# To Run:
npm install
npm run start

# To deploy to cloud.gov
- npm run build
- upload resulting dist folder to cloud.gov
- make sure the following manifest.yml is included in the dist folder:
applications:
- name: CobraApp
  memory: 64M
  buildpacks:
  disk_quota: 1G
  - staticfile_buildpack


- you may change name to CobraDev for dev/staging deployment and use CobraApp for production deployment


### What is this repository for? ###

* Quick summary
* Version
* [Learn Markdown](https://bitbucket.org/tutorials/markdowndemo)

### How do I get set up? ###

* Summary of set up
* Configuration
* Dependencies
* Database configuration
* How to run tests
* Deployment instructions

### Contribution guidelines ###

* Writing tests
* Code review
* Other guidelines

### Who do I talk to? ###

* Repo owner or admin
* Other community or team contact

ng build --configuration production

cf login -a api.fr.cloud.gov --sso
cf push COBRAPROTOTYPE -b staticfile_buildpack


