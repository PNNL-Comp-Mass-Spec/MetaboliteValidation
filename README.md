# metaboliteValidation
This is a simple c# console program to validate and update the data from [MetabolomicsCCS](https://github.com/PNNL-Comp-Mass-Spec/MetabolomicsCCS)

## usage
To use this program you must first install [Python](https://www.python.org/downloads/) and [goodtables](https://pypi.python.org/pypi/goodtables), and add the folder path to goodtables.exe to an environment variable named GOODTABLES_PATH.
The default location for goodtables.exe will be ```<path to python>/Scripts``` After everything is properly installed open the command promt and run:

```
metaboliteValidation <File path to new data>
```

# UnitTestMetaboliteValidation
A unit test project for [metaboliteValidation](https://github.com/PNNL-Comp-Mass-Spec/metaboliteValidation)
