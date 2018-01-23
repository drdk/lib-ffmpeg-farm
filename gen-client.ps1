Write-Host "Client gen.."
If (!(Test-Path "NSWag")) {
    Write-Host "NSwag not found downloading from github..."
    $filename = (Get-Location).Path +"\NSwag.zip"
    $client = new-object System.Net.WebClient
    $client.DownloadFile("https://github.com/RSuter/NSwag/releases/download/NSwag-Build-956/NSwag.zip","$filename")    
    
    new-item -Name "NSWag" -ItemType directory
    Write-Host "Unzipping to nswag dir..."
    $shell_app=new-object -com shell.application    
    $zip_file = $shell_app.namespace("$filename")
    $destination = $shell_app.namespace((Get-Location).Path +"\Nswag")
    $destination.Copyhere($zip_file.items())
}

cd NSwag\win
Write-Host "Generating client with nswag..."
&.\nswag.exe swagger2csclient /input:http://localhost:9000/swagger/docs/v1 /InjectHttpClient:true /classname:"{controller}Client" /namespace:DR.FFMpegClient /output:..\..\src\FFMpegClient\FFMpegClient.cs
cd ..\..
Write-Host "Done"