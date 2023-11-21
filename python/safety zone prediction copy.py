from flask import Flask, request, jsonify
import pandas as pd
import googlemaps
import os
import requests
from io import StringIO

app = Flask(__name__)

# Load API key from environment variable for better security
api_key = os.getenv('GOOGLE_MAPS_API_KEY')
if not api_key:
    raise ValueError("No API key found. Set the GOOGLE_MAPS_API_KEY environment variable.")
gmaps = googlemaps.Client(key=api_key)

def fetch_hurricane_data(csv_url):
    response = requests.get(csv_url)
    csv_content = StringIO(response.content.decode('utf-8'))
    return pd.read_csv(csv_content)

def find_infrastructure(longitude, latitude, radius=5000):
    places_result = gmaps.places_nearby(location=(latitude, longitude), radius=radius, type='establishment')
    return places_result

def get_elevation(longitude, latitude):
    elevation_result = gmaps.elevation((latitude, longitude))
    return elevation_result

def get_distance_from_coast(longitude, latitude, coast_coordinates):
    distance_result = gmaps.distance_matrix(origins=(latitude, longitude), destinations=coast_coordinates, mode="driving")
    return distance_result

def find_nearest_infrastructure_safe_zone(longitude, latitude, hurricane_category, gmaps_client):
    safe_infrastructure_types = ['hospital', 'school', 'community_centre', 'local_government_office', 'university', 'church', 'fire_station', 'police']
    category_min_distances = {'3': 37.5, '4': 71.5, '5': 80}  # Minimum distances in miles
    min_distance_miles = category_min_distances.get(str(hurricane_category), 0)
    search_radius_miles = max(30, min_distance_miles)  # Start with at least 30 miles
    search_radius_meters = search_radius_miles * 1609.34

    for infrastructure_type in safe_infrastructure_types:
        infrastructure_results = gmaps_client.places_nearby(location=(latitude, longitude), radius=search_radius_meters, type=infrastructure_type)

        safe_places = [place for place in infrastructure_results.get('results', []) if place['geometry']['location']]

        if safe_places:
            nearest_safe_place = safe_places[0]
            nearest_safe_place_location = nearest_safe_place['geometry']['location']
            directions_result = gmaps_client.directions(origin=f"{latitude},{longitude}", destination=f"{nearest_safe_place_location['lat']},{nearest_safe_place_location['lng']}", mode="driving")
            return directions_result, nearest_safe_place

    return None, None

@app.route('/runRiskAssessment', methods=['GET'])
def run_risk_assessment_api():
    longitude = request.args.get('longitude', type=float)
    latitude = request.args.get('latitude', type=float)
    hurricane_category = request.args.get('category', type=int)

    csv_url = 'https://raw.githubusercontent.com/jeanstephanelopez/google-geospacial/main/saffir_simpson_hurricane_scale.csv'
    hurricane_data = fetch_hurricane_data(csv_url)

    directions_to_safe_zone, safe_place = find_nearest_infrastructure_safe_zone(longitude, latitude, hurricane_category, gmaps)
    
    if not safe_place or not directions_to_safe_zone:
        return jsonify({"message": "No safe place or directions could be provided."})

    safe_place_details = {
        "name": safe_place.get('name'),
        "address": safe_place.get('vicinity'),
        "directions": [step.get('html_instructions') for step in directions_to_safe_zone[0]['legs'][0]['steps']]
    }
    return jsonify(safe_place_details)

if __name__ == "__main__":
    app.run(debug=True)
