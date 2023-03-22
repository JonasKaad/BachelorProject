#!/usr/bin/env python3
import requests
import json
import time
import argparse

endpoint = "https://traffic-service.acqa.foreflight.com/traffic/v1/Aircraft/history/"
delay_between_requests_seconds = 0
output_direcory = "out/"

def fetchFlights(fileName, startAt):
    print("Reading " + fileName)
    f = open(fileName, "r")
    flightIds = json.loads(f.read())
    f.close()

    print("Found " + str(len(flightIds)) + " flight ids!")
    
    totalIds = len(flightIds)
    fetchedIds = 0
    for flightId in flightIds:
        if startAt != None:
            if flightId == startAt:
                startAt = None
            else:
                fetchedIds = fetchedIds + 1
                print("("+str(fetchedIds)+"/"+str(totalIds)+") Skipping " + flightId)
                continue

        rawFlightJson = fetchFlight(flightId)
        outFileName = str(flightId) + ".json"
        outPath = output_direcory + outFileName

        flightFile = open(outPath, "w")
        flightFile.write(rawFlightJson)
        flightFile.close()

        fetchedIds = fetchedIds + 1
        print("("+str(fetchedIds)+"/"+str(totalIds)+") Fetched and saved flight " + str(flightId) + " => " + outPath)
        time.sleep(delay_between_requests_seconds)

    print("Done! Wow!")


def fetchFlight(id):
    return str(requests.get(endpoint + str(id)).text)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
                    prog='Get Flights',
                    description='Fetches flights from FF given a JSON file containing an array of strings')
    
    parser.add_argument("filename")
    parser.add_argument("-s", "--start-at", required=False, help="If set, will skip all entries untill specified argument")

    args = parser.parse_args()

    fetchFlights(args.filename, args.start_at)
