import os
projectDirectory = "D:/HBP/HiBoP/"

try:
	os.system("D:/Programs/Unity/Editor/Unity.exe -batchmode -quit -logfile D:/HBP/HiBoP_builds/log.log -projectPath " + projectDirectory + " -executeMethod Tools.Unity.HBPBuilder.DevelopmentBuild")
	os.system("pause")
except OSError as e:
	print(e)