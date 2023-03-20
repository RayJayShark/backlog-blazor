/**
* JetBrains Space Automation
* This Kotlin-script file lets you automate build activities
* For more info, see https://www.jetbrains.com/help/space/automation.html
*/

job("Build") {
	container(displayName = "Build Instance", image = "mcr.microsoft.com/dotnet/sdk:7.0.102-jammy-amd64") {

    	shellScript {
  			content = """
				cd Backlog-Blazor
    			wget -O tailwindcss https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-linux-x64
       			ls
          		chmod +x tailwindcss
            	cd ..
    			dotnet publish -o build
     		"""
      	}
    }

    startOn {
    	gitPush { enabled = true }
    }
}