# MetaboliteValidation

This is a simple C# console program to validate and update the data from [MetabolomicsCCS](https://github.com/PNNL-Comp-Mass-Spec/MetabolomicsCCS)

## Usage
To use this program you must first install [Python](https://www.python.org/downloads/) and [goodtables](https://pypi.python.org/pypi/goodtables), and add the folder path to goodtables.exe to an environment variable named GOODTABLES_PATH.
The default location for goodtables.exe will be ```<path to python>/Scripts``` After everything is properly installed open the command promt and run:

```
MetaboliteValidation <File path to new data>
```

# UnitTestMetaboliteValidation

A unit test project for [MetaboliteValidation](https://github.com/PNNL-Comp-Mass-Spec/MetaboliteValidation)

## License

The Metabolite Validation program is licensed under the Apache License, Version 2.0;
you may not use this program except in compliance with the License.  You may obtain
a copy of the License at https://opensource.org/licenses/Apache-2.0
