# Discord Compressor

Quickly compress your videos for Discord! [Download Here](https://github.com/ethantreece/DiscordCompressor/releases)

Great for sending your favorite recent clips to your friends without the fuss of trimming the video down and lowering the resolution.
Right click any .mp4 file, pick an option from 'Compress for Discord', and the compressed video is saved as '<filename>_compressed.mp4'.

Includes options for upload limits based on boosted server levels (25mb, 50mb, 100mb).

![sample test](https://github.com/ethantreece/DiscordCompressor/assets/38461748/d517dc0c-462f-45dd-bd2e-880cfe07b25d)

#### Information
InnoScript was used to build the installer for the application. You could publish the code yourself and alter the InnoScript.iss by pointing the files to the correct directory and compile the installer.

The video is compressed using FFmpeg and the Two-Pass encoding method. The temporary files seen while compressing are created in the first pass to analyze the video information and allow more accurate compression in the second pass.
