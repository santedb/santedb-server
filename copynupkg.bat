
FOR /R "." %%G IN (*.nupkg) DO (
	copy %%G %localappdata%\NugetStaging
)
FOR /R "." %%G IN (*.snupkg) DO (
	copy %%G %localappdata%\NugetStaging
)