# EasyGameProxy

EasyGameProxy is a simple tool that allows you to reverse proxy TCP connections.

Want to run multiple services on different subdomains without providing weird port numbers? Try this!



## Installation

It's recommended to use Docker Compose for this. CLI is fine too, but you should follow the parameters below.


- Volumes:
  - You'll want to set up a volume for the /app/config folder. This folder contains your routing configuration file (routes.json). Setting up a persistent volume will allow you to keep your changes.
- Ports:
  - From here, you'll list ports that you want exposed. If we're hosting a Minecraft server, you'd just use 25565. The web UI runs on 8080 by default.
- Environment (optional)
  - ASPNETCORE_URLS=http://+:9000 
  - The above is needed if you want to override the default web UI port. Don't forget to change your exposed port as well.

## Configuration File (routes.json)
I've included a sample at the bottom of this readme. The EasyGameProxy services runs in the background and detects when the file is changed, adding new entries without the need to restart the container. However, you should note that even though you added the route, you still need to expose the port of the container through your compose file. If you plan on exposing a bunch of services, plan your compose file ahead of time to include the necessary ports, or include a range of ports.

- listenPort - Which port EasyGameProxy should listen on
- hostname - The hostname that we should try to route traffic for
- host - The IP of the actual host running the desired service
- port - The destination port of the desired service

## DNS
I personally use PiHole for my local DNS records. In PiHole, I just route whichever hostname (like mc1.abc.com) to the server running my EasyGameProxy container. That's it! Just create your DNS entries and modify the routes.json file as needed.



## Sample docker-compose.yaml
```yaml
services:
  easygameproxy:
    image: maknuppel/easygameproxy:latest
    volumes:
      - "./config:/app/config"
    ports:
      - "25565:25565"
      - "8080:8080"
```
Want to run the web UI on a different port? Try below, replacing 9000 with whichever port you want:
```yaml
    ports:
      - "25565:25565"
      - "9000:9000"
    environment:
      - ASPNETCORE_URLS=http://+:9000
```

## Sample routes.json File
This sample file allows you to route requests coming from a single exposed port, 25565. In this example, 2 servers are running on a single node but exposing different ports. By creating DNS records to point mc1.abc.com and mc2.abc.com to the EasyGameProxy host, EasyGameProxy will then route traffic based on the hostname being requested.

```json
"routes": [
  {
    "listenPort": 25565,
    "hostname": "mc1.abc.com",
    "host": "10.0.0.2",
    "port": 25566
  },
  {
    "listenPort": 25565,
    "hostname": "mc2.abc.com",
    "host": "10.0.0.2",
    "port": 25568
  }
]
```

