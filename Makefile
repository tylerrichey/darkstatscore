ROOT	:= $(abspath $(dir $(lastword $(MAKEFILE_LIST))))
NAME	:= darkstatscore
TAG 	:= tylerrichey/$(NAME)

.PHONY: all build

all: build

build:
	@docker build -t $(TAG) $(ROOT)

run:
	@docker run -it -d --restart always -v "/Users/teeman/darkstatscore":/app/db -p 10.0.0.200:6677:6677 --name $(NAME) $(TAG) 

clean:
	@docker stop $(NAME)
	@docker rm -v $(NAME)
	@docker rmi -f $(TAG)