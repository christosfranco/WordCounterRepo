# Problem Description
Technical implementation based on the description given
Description:
```
Write and test a C# program that:
Given multiple text files, will count the occurrences of each unique word in the files and aggregate the results. Processing of the data should be done in a way to minimize running time, independent of the number of files or file size.
 For example, consider two files:
  - File 1 containing the text “Go do that thing that you do so well”
  - File 2 containing the text “I play football well”
-- It should find these counts: 
1: Go
2: do
2: that
1: thing
1: you
1: so
2: well
1: I
1: play
1: football
```

# Compile and run
Set-up and compile:
```
dotnet build --configuration Release --no-restore
dotnet run [FILES]
```
Example:
```
dotnet run ..\TestWordCounter\TestFiles\file1.txt ..\TestWordCounter\TestFiles\file2.txt 
```
# Run build-in tests:
```
dotnet test --no-restore --verbosity normal
```


# Utilizing testing pipeline
For documentation and purpose of the code that I have written for other developers looking at it. (aswell as myself when i return to the codebase)
Compile, run, and test the solution. Pipeline can also be ran directly through the github actions interface on [link](https://github.com/christosfranco/File_Word_Counter/actions)
Or through a local program such as "act".

To run the github actions pipeline:
```
act 
```