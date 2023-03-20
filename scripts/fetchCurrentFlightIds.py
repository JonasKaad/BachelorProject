#!/usr/bin/env python3
import requests
import json
import time
import math

endpoint = "https://traffic-streaming.acqa.foreflight.com/traffic/v1/position/DEV/all"

def main():
    print("Fetching from " + endpoint)
    response = requests.get(endpoint)
    content = response.json()

    ids = []

    for flight in content:
        ids.append(flight["id"])

    print("Fetched " + str(len(ids)) + " ids.")

    fileName = "flights_" + str(math.floor(time.time())) + ".json"

    print("Saving to " + fileName)

    f = open(fileName, "w")
    f.write(json.dumps(ids))
    f.close()

    print("Done!")


if __name__ == "__main__":
    main()