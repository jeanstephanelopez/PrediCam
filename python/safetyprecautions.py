import os
import requests
import json
import math

class HurricaneSafetyProtocol:
    def __init__(self, csv_line):
        values = csv_line.split(',')
        self.Category = int(values[0])  # 'category' in CSV
        self.SustainedWinds = int(values[1])  # 'Sustained Winds (mph)' in CSV
        self.SizeInMiles = int(values[2])  # 'size_in_miles' in CSV
        self.SafetyProtocols = values[3]  # 'Safety Protocols' in CSV
        self.ProximityToWater = values[4]  # 'Proximity to Water' in CSV
        self.InfrastructureType = values[5]  # 'Infrastructure Type' in CSV

# Retrieve the Google Maps API key from the environment variable
google_places_api_key = os.environ.get('GOOGLE_MAPS_API_KEY')

class LocationSafetyAnalysis:
    def __init__(self):
        self.google_places_api_key = google_places_api_key
        self.safety_protocols = []
        self.water_bodies_response = None
        self.infrastructure_response = None
        self.user_latitude = None
        self.user_longitude = None
        self.hurricane_category = None

    def set_user_location(self, latitude, longitude):
        self.user_latitude = latitude
        self.user_longitude = longitude

    def set_hurricane_category(self, category):
        self.hurricane_category = category

    def load_data(self):
        with open("safety_protocols_simulation.csv", "r") as file:
            next(file)  # Skip header line
            for line in file:
                protocol = HurricaneSafetyProtocol(line.strip())
                self.safety_protocols.append(protocol)

    def find_nearby_water_bodies(self):
        url = f"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={self.user_latitude},{self.user_longitude}&radius=5000&type=lake&key={self.google_places_api_key}"
        response = requests.get(url)
        if response.status_code == 200:
            self.water_bodies_response = response.json()
        else:
            print("Error:", response.text)

    def process_water_bodies_response(self):
        if "results" not in self.water_bodies_response or not self.water_bodies_response["results"]:
            return "Far"

        closest_distance = float('inf')
        for result in self.water_bodies_response["results"]:
            location = result["geometry"]["location"]
            water_body_lat = location["lat"]
            water_body_lng = location["lng"]
            distance = self.calculate_distance(
                self.user_latitude, self.user_longitude, water_body_lat, water_body_lng)
            if distance < closest_distance:
                closest_distance = distance

        return self.categorize_proximity(closest_distance)

    @staticmethod
    def calculate_distance(lat1, lon1, lat2, lon2):
        R = 6371
        dLat = math.radians(lat2 - lat1)
        dLon = math.radians(lon2 - lon1)
        a = math.sin(dLat / 2) ** 2 + math.cos(math.radians(lat1)) * math.cos(math.radians(lat2)) * math.sin(dLon / 2) ** 2
        c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))
        return R * c

    @staticmethod
    def categorize_proximity(distance):
        if distance < 1:
            return "Close"
        elif distance < 5:
            return "Moderate"
        else:
            return "Far"

    def find_nearby_infrastructure(self):
        url = f"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={self.user_latitude},{self.user_longitude}&radius=1000&key={self.google_places_api_key}"
        response = requests.get(url)
        if response.status_code == 200:
            self.infrastructure_response = response.json()
        else:
            print("Error:", response.text)

    def process_infrastructure_response(self):
        if "results" not in self.infrastructure_response or not self.infrastructure_response["results"]:
            return "Residential"  # Default to Residential if no data

        residential_count, commercial_count, public_area_count = 0, 0, 0
        for result in self.infrastructure_response["results"]:
            types = result.get("types", [])
            for place_type in types:
                if self.is_residential(place_type):
                    residential_count += 1
                elif self.is_commercial(place_type):
                    commercial_count += 1
                elif self.is_public_area(place_type):
                    public_area_count += 1

        return self.determine_predominant_type(residential_count, commercial_count, public_area_count)

    @staticmethod
    def is_residential(place_type):
        residential_types = ["lodging", "homes", "neighborhood"]
        return place_type in residential_types

    @staticmethod
    def is_commercial(place_type):
        commercial_types = ["store", "shopping_mall", "office", "restaurant", "supermarket", "bank", "hotel", "apartment", "home", "school", "university", "hospital", "clinic", "government", "embassy", "courthouse", "library", "museum", "church"]
        return any(type in place_type for type in commercial_types)

    @staticmethod
    def is_public_area(place_type):
        public_area_types = ["park", "recreation_area", "public_square", "nature_reserve", "zoo", "stadium"]
        return any(type in place_type for type in public_area_types)

    @staticmethod
    def determine_predominant_type(residential, commercial, public_area):
        if residential >= commercial and residential >= public_area:
            return "Residential"
        elif commercial >= residential and commercial >= public_area:
            return "Commercial"
        else:
            return "Public Area"

    def determine_safety_protocols(self, hurricane_category, proximity_to_water, infrastructure_type):
        # First, find protocols matching the hurricane category
        category_matched_protocols = [protocol for protocol in self.safety_protocols if protocol.Category == hurricane_category]

        # Then, further refine matches based on proximity to water and infrastructure type
        refined_matches = [protocol for protocol in category_matched_protocols if protocol.ProximityToWater.lower() == proximity_to_water.lower() or protocol.InfrastructureType.lower() == infrastructure_type.lower()]

        # If no refined matches, return all category matched protocols
        matched_protocols = refined_matches if refined_matches else category_matched_protocols
        unique_protocols = list(set([protocol.SafetyProtocols for protocol in matched_protocols]))

        # Limiting the output to 3 unique protocols
        return unique_protocols[:3]


    def analyze_location_safety(self):
        self.find_nearby_water_bodies()
        proximity_to_water = self.process_water_bodies_response()

        self.find_nearby_infrastructure()
        infrastructure_type = self.process_infrastructure_response()

        safety_protocols = self.determine_safety_protocols(
            self.hurricane_category, proximity_to_water, infrastructure_type)

        # Print the desired output
        print(f"Based on {infrastructure_type} and {proximity_to_water} distance from water bodies, matched protocols are: {safety_protocols if safety_protocols else 'No specific protocols found.'}")

if __name__ == "__main__":
    analysis = LocationSafetyAnalysis()
    analysis.load_data()
    analysis.set_user_location(29.651980, -82.325020)  # Set a specific location
    analysis.set_hurricane_category(1)  # Set a specific hurricane category
    analysis.analyze_location_safety()
