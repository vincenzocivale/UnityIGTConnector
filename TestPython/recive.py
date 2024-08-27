import pandas as pd

df1 = pd.read_csv('value.csv', header=None, on_bad_lines='skip')


print(f"Valore massimo: {df1.max()}  ")
print(f"Valore minimo: {df1.min()}  ")

