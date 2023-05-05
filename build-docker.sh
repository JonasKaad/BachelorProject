#!/bin/bash
if [ -z "$1" ]
  then
    echo "Please supply a tag verison. Example: ./build-docker.sh v2"
    exit
fi

docker build -t 051153811601.dkr.ecr.us-east-1.amazonaws.com/anorup-bachelorproject:$1 .

echo "Built as 051153811601.dkr.ecr.us-east-1.amazonaws.com/anorup-bachelorproject:$1"
echo "When ready, push using command below"
echo ""
echo "docker push 051153811601.dkr.ecr.us-east-1.amazonaws.com/anorup-bachelorproject:$1"
echo ""
echo "If you don't have credentials in Docker, or they expired, use the following command"
echo ""
echo "aws-vault exec ac-qa-poweruser -- aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 051153811601.dkr.ecr.us-east-1.amazonaws.com"