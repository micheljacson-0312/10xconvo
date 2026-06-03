#!/bin/bash
docker rm -f $(docker ps -aq) || true
docker rmi -f $(docker images -q) || true
docker volume rm $(docker volume ls -q) || true
docker system prune -a -f
