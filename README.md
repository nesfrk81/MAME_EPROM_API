# MAME_EPROM_API
This API is designed to try to collect EPROM information from the mame github repository.

If you run this project in Visual Studio, it will open up the Swagger UI when started.
If you are using the release build, run EPROM_API.exe and connect to http://localhost:5000/swagger/index.html

Select the /Eprom endpoint and click "Try it out".
Enter the mameGameDriver, for example 1943, and press execute.

The API will look up that driver in the mame repository. (https://github.com/mamedev/historic-mame/blob/master/src/mame/drivers/1943.c)

Some of the mapped EPROM types does not apply to the real world, but it would give you a good indication on what to expect when patching a rom on a real PCB.

Current know issues:
- Much of the code is AI generated, cleaning up is needed.
- Missing 16-bit EPROM mappings, need to investigate if this is possible to map from the mame driver.
