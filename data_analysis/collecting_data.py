import numpy as np
from datetime import datetime

# discretization of continuous data
window_in_minutes = 10
window_speed = 1

# Index data
pullIdx = 7
dropoffIdx = 8
trip_distance_idx = 4
pullTimeIdx = 1
dropTimeIdx = 2

# eliminating outliers in speed data
max_speed = 60

numbers = list()
number_of_locations = 265 # Number of total locations according 
https://catalog.data.gov/dataset/2019-20-demograhic-snapshot-district/resource/15414b2b-f6ca-4b3a-8f9e-b7e8927e87a7

# data
transition_matrix = np.zeros(dtype="float32", 
shape=(number_of_locations, number_of_locations))
events_per_location = np.zeros(dtype="int64", 
shape=(number_of_locations,))
time_histogram = 
np.zeros(dtype="float32",shape=(24*60//window_in_minutes,))
speed_histogram = np.zeros(dtype="float32",shape=(max_speed+1,))


filename  = './taxi_data_50mb.csv'
header = True


def add_trip_to_matrix(pullLocStr, dropLocStr):
    pullLoc = int(pullLocStr) - 1
    dropLoc = int(dropLocStr) - 1
    events_per_location[pullLoc] = events_per_location[pullLoc] + 1
    transition_matrix[pullLoc][dropLoc] = 
transition_matrix[pullLoc][dropLoc] + 1


def add_trip_time(pull_time):
    pull_time_slot = time_slot(pull_time)
    time_histogram[pull_time_slot] = time_histogram[pull_time_slot] + 
1.0


def add_trip_speed(trip_distance, duration):
    speed = trip_distance / (duration / 60) if duration != 0 else 0
    speed_slot = int(speed)
    if speed < max_speed:
        speed_histogram[speed_slot] = speed_histogram[speed_slot] + 1.0


def time_slot(datetime_obj):
    return (datetime_obj.hour * 60 + 
datetime_obj.minute)//window_in_minutes


with open(filename, 'r') as file:
    for line in file:
        line = line.split(",")
        if header:
            print(line)
            header = False
        else:
            add_trip_to_matrix(line[pullIdx], line[dropoffIdx])
            pull_date_time = datetime.strptime(line[pullTimeIdx], 
'%m/%d/%Y %I:%M:%S %p')
            drop_date_time = datetime.strptime(line[dropTimeIdx], 
'%m/%d/%Y %I:%M:%S %p')
            duration = (drop_date_time - pull_date_time).total_seconds() 
/ 60
            trip_distance = float(line[trip_distance_idx])
            if duration >= 0:
                add_trip_time(pull_date_time)
                add_trip_speed(trip_distance, duration)

# normalizing matrix
for p in range(len(events_per_location)):
    if events_per_location[p] > 0:
        transition_matrix[p, :] = transition_matrix[p, :] / 
events_per_location[p]

# normalizing histograms
time_histogram = time_histogram[:] / sum(time_histogram)
speed_histogram = speed_histogram[:]  / sum(speed_histogram)

# saving matrix and histograms
np.save('taxis_transition_matrix.npy', transition_matrix)
np.save('taxis_speed_histogram.npy', speed_histogram)
np.save(f'taxis_time_histogram_window_of_{window_in_minutes}_minutes.npy', time_histogram)
np.save(f'taxis_ride_origin_counting.npy', events_per_location)


