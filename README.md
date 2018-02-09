[![Build status](https://ci.appveyor.com/api/projects/status/w4kk1co6igx7t3tu/branch/master?svg=true)](https://ci.appveyor.com/project/rbnswartz/crmwebresourceupload/branch/master)
# CRMWebResourceUpload
A utility to upload and create web resources to dynamics crm

# Desciption
Uploading multiple web resources to dynamics crm is a bit of a hassel and gets even more difficult if you want to do so in 
a CD pipleline. So I wrote this piece of software to upload all of the resources in a folder (or set of folders) in order to integrate
with our internal build pipeline.

# Building
The easiest way to build is to use Visual Studio to compile the solution.

# Usage
This is a command line tool so you are going to need to open an instance of command prompt in order to run it.

Usage:

`CRMWebResourceUpload.exe <CRM Connection String> <Target Solution> <Source Directory>`

Example:

`CRMWebResourceUpload.exe "AuthType=Office365; Url=server.crm.dynamics.com; Username=user@domain.com; Password=password" CustomerSolution build`

The above example will log into the Dynamics crm server located at server.crm.dynamics.com 
with the supplied username and password, load all of the web resources from the build 
folder and put them in the CustomerSolution solution. 

This also processes through all of the child folders also and create a directory structure in CRM that matches it. 

For instance if I had the following structure
- new_
    - scripts
        - contact.js
        - account.js
    - views
        - sheep.html

You would end up with the following  web resources

- new_/scripts/contact.js
- new_/scripts/account.js
- new_/views/sheep.html

# Contributing
If you find a bug or need additional functionality please submit an issue. If you are feeling
generous or adventurous feel free to fix bugs or add functionlity in your own fork and send us
a pull request.