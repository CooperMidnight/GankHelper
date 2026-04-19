# Intro
This is a .NET CLI app that removes some of the monotony around a few of my tasks on the [Gank](https://ganknow.com) platform.

# How to use
Edit `appsettings.json` before doing anything. You will need to get a valid bearer token for your account, which is how this
application will authenticate with the Gank REST API. You can do this by going to the Gank website, opening developer tools (F12),
going to the network tab, navigating somewhere in the website, and finding a network request that uses the API. Go to its request
headers, copy the value for "Authorization", remove the "Bearer " prefix (including the space), and paste it into the appsettings
file. If you intend to use the Get Sales command and write to a CSV file, ensure that `ShouldWriteToCsvFile` is set to `true` and
that `CsvFilePath` is valid.

# Commands

## Get Sales
This command iterates through all of the incoming money transactions you've ever had and then
1. Outputs (to the console) the amount of money (sans fees) you've made from product sales, tips, etc.
2. Outputs (to the console) the amount of money (sans fees) you've withdrawn from Gank
3. Outputs (to a CSV file) how many times each item has sold and the total amount of money (sans fees) it's made

## Reorder items
This command retrieves all of your listings and then orders them by name, optionally according to 1 or more substrings they may contain.
