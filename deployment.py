import os
import json
import ftplib

firstDeployment = False
source = "bin/Debug/netcoreapp2.1/publish"
destination = "site/wwwroot/"
config = json.load(open("appcredentials.json", "r"))

session = ftplib.FTP(config["endpoint"], config["username"], config["password"])
allFiles = [os.path.join(dp, f).replace("\\", "/") for dp, dn, filenames in os.walk(source) for f in filenames]
for f in allFiles:
	file = open(f, "rb")
	childname = str.replace(f, source+"/", "")
	session.storbinary("STOR " + destination + childname, file)
	print("Uploaded at ...  ", destination+childname)
print("Done uploading ", len(allFiles), "files")
session.quit()

open("Controllers/MainController.cs", "w").write(open("BkpMainController.txt", "r").read())
print("Cleanup done")