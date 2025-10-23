.DEFAULT_GOAL := help

help:  ##Display this help
	@awk 'BEGIN {FS = ":.*##"; printf "\nUsage:\n  make \033[36m<target>\033[0m\n\nTargets:\n"} /^[a-zA-Z_-]+:.*?##/ { printf "  \033[36m%-10s\033[0m %s\n", $$1, $$2 }' $(MAKEFILE_LIST)

build: ##Build Docker
	sudo docker build -t pcm .
	
run: ##Run Docker
	sudo docker run -d -p 9292:80 --name pcm

stop: ##Stop Docher
	sudo docker kill pcm && docker rm $(sudo docker ps -a --filter "ancestor=pcm" -q)

restart: ##Restart docker
	sudo docker kill pcm && docker rm $(sudo docker ps -a --filter "ancestor=pcm" -q) && sudo docker run -d -p 9292:80 --name pcm

logs: ##Connect to PHP
	sudo docker logs --follow pcm
