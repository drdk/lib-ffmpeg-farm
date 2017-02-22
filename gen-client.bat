IF NOT EXIST NSwag (
    mkdir NSwag
    cd NSwag
    unzip ..\NSwag.zip
) ELSE (
    cd NSwag
)
nswag swagger2csclient /input:http://localhost:9000/swagger/docs/v1 /classname:{controller}Client /namespace:DR.FFMpegClient /output:..\src\FFMpegClient\FFMpegClient.cs
cd .. 