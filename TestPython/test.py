import pyigtl
client = pyigtl.OpenIGTLinkClient("localhost", 18945)
message = client.wait_for_message("Image")
print(message)