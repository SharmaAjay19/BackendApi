import json

dic = json.load(open("dbcredentials.json", "r"))
fp = open("Controllers/MainController.cs", "r")
content = fp.read()
bkp = open("BkpMainController.txt", "w")
bkp.write(content)
bkp.close()
for key in dic.keys():
	content = content.replace(key, dic[key])
fp.close()
fp = open("Controllers/MainController.cs", "w")
fp.write(content)
fp.close()