/**
* JetBrains Space Automation
* This Kotlin-script file lets you automate build activities
* For more info, see https://www.jetbrains.com/help/space/automation.html
*/

job("Build") {
	container(displayName = "Build Solution", image = "mcr.microsoft.com/dotnet/sdk:7.0.102-jammy-amd64") {

    	shellScript {
  			content = """
				cd Backlog-Blazor
    			wget -O tailwindcss https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-linux-x64
          		chmod +x tailwindcss
    			dotnet publish -o build -c Release -r linux-x64 --self-contained
       			cd ../BacklogBlazor_Server
          		dotnet publish -o build -c Release -r linux-x64 --no-self-contained
     		"""
      	}
    }

    startOn {
    	gitPush { 
        	branchFilter {
				-"refs/heads/main"
            }
        }
    }
}

job ("Deploy") {
	parameters {
		secret("PROD_KEY", value = "{{ project:PROD_KEY }}")
    }

  
    container(displayName = "Build Solution", image = "mcr.microsoft.com/dotnet/sdk:7.0.102-jammy-amd64") {

    	shellScript {
  			content = """
				cd Backlog-Blazor
    			wget -O tailwindcss https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-linux-x64
          		chmod +x tailwindcss
    			dotnet publish -o ${'$'}JB_SPACE_FILE_SHARE_PATH/Backlog-Blazor/build -c Release -r linux-x64 --self-contained
       			cd ../BacklogBlazor_Server
          		dotnet publish -o ${'$'}JB_SPACE_FILE_SHARE_PATH/BacklogBlazor_Server/build -c Release -r linux-x64 --no-self-contained
     		"""
      	}
    }
  
	host("Deploy") {
      	fileInput {
			source = FileSource.Text("{{ project:PROD_KEY}}")
            localPath = "/root/.ssh/id_rsa"
        }
        fileInput {
			source = FileSource.Text("{{ project:ENV_FILE }}")
            localPath = "/root/.env"
        }

    	shellScript {
			content = """
                chmod 700 /root/.ssh/id_rsa

             	ssh -oStrictHostKeyChecking=no -v -i /root/.ssh/id_rsa deploy@game.tacolog.app "sudo rm -rf /var/www/game.tacolog.app/*"
              	scp -v -r -i /root/.ssh/id_rsa ${'$'}JB_SPACE_FILE_SHARE_PATH/Backlog-Blazor/build/* deploy@game.tacolog.app:/var/www/game.tacolog.app
    			ssh -v -i /root/.ssh/id_rsa deploy@game.tacolog.app "sudo systemctl stop tacologapi.service"
               	ssh -v -i /root/.ssh/id_rsa deploy@game.tacolog.app "sudo rm -rf /var/www/gameapi.tacolog.app/*"
                scp -v -r -i /root/.ssh/id_rsa ${'$'}JB_SPACE_FILE_SHARE_PATH/Backlog-Blazor/build/* deploy@game.tacolog.app:/var/www/game.tacolog.app
                scp -v -i /root/.ssh/id_rsa /root/.env deploy@game.tacolog.app:/var/www/gameapi.tacolog.app
                ssh -v -i /root/.ssh/id_rsa deploy@game.tacolog.app "sudo systemctl start tacologapi.service"
   			"""
      	}
    }

    startOn {
      	gitPush {
			branchFilter {
				+"refs/heads/main"
            }
        }
    }
}