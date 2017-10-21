# DarkStatsCore
A bandwidth monitoring tool in ASP.NET Core.

[![Docker Automated buil](https://img.shields.io/docker/automated/tylerrichey/darkstatscore.svg)](https://hub.docker.com/r/tylerrichey/darkstatscore/)
[![Docker Build Statu](https://img.shields.io/docker/build/tylerrichey/darkstatscore.svg)](https://hub.docker.com/r/tylerrichey/darkstatscore/)

![darkstatscore](https://user-images.githubusercontent.com/11445915/31852482-56177398-b646-11e7-9e30-784382b18eca.png)

The intent of this project was to take the data produced by '[darkstat](https://unix4lyfe.org/darkstat/)' and display it in a more informative, friendly, format. DarkStatsCore collects data often and stores it by hour, so you can break down bandwidth usage by day or month. There is also a 'live dashboard' feature that will give you an immediate insight in to current network traffic.

I arrived at this point due to Comcast's stupid data caps and the inability to find a good enough tool to run on my [EdgeRouter X](https://www.ubnt.com/edgemax/edgerouter-x/) to provide this data.

To use DarkStatsCore, I recommend you restart your 'darkstat' instance every night (to have it write its database file), and feed it a new database file every month. Otherwise, the numbers get enormous and will only lead to headaches. I will share my scripts for this at the bottom of this README.

To start using this, run something like:

```docker run -it -d --restart always -v "/your/machine/darkstatscore/db":/app/db -p 6677:6677 tylerrichey/darkstatscore```

By default, the container will use the America/New_York timezone. If you live in another timezone, use the -e option and override the TZ environment variable.

I'm not currently building any other executables for this project, but you can run it from source with:

```
git clone https://github.com/tylerrichey/darkstatscore.git
cd darkstatscore
dotnet restore
cd DarkStatsCore
bower install
dotnet run <optional argument to specify a different port, i.e., http://*:8080>
```

Here is how I run 'darkstat'; my /etc/darkstat/init.cfg:
```
START_DARKSTAT=yes
INTERFACE="-i switch0"
DIR="/var/lib/darkstat"
LOCAL="-l 10.0.0.0/255.255.255.0"
DAYLOG="--daylog darkstat.log"
FILTER="not (src net 10.0.0 and dst net 10.0.0)"
OPTIONS="--local-only"
```

This is my daily restart script:
```
#!/bin/bash

sudo service darkstat restart
```

This is my monthly restart script:
```
#!/bin/bash

sudo service darkstat stop
mv /var/lib/darkstat/darkstat.db /var/lib/darkstat/darkstat.$(date +"%m_%d_%Y").db
sudo service darkstat start
```

If you use an EdgeRouter, this is my task-scheduler configuration:
```
task-scheduler {
	task dailydarkstat {
		crontab-spec "0 0 * * *"
		executable {
			path /config/user-data/scripts/darkstatsrestart.sh
		}
	}
	task monthlydarkstat {
		crontab-spec "0 0 1 * *"
		executable {
			path /config/user-data/scripts/darkstatsmonthly.sh
		}
	}
}
```
