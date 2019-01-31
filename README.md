# SGEHealthCheckService
Windows service app to poll network machines and write CPU usage, C: Drive Space and Memory usage to a json file.  Built using .Net 4.5 and Newtonsoft's Json.Net 4.6
Must have a "C:\Program Files\SGE Health Check" directory.  Must have a file named "servers.txt" in the "C:\Program Files\SGE Health Check\input" directory.  