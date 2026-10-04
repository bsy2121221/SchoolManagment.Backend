param(
    [Parameter(Mandatory = $true)]
    [string]$Server,

    [Parameter(Mandatory = $true)]
    [string]$Username,

    [Parameter(Mandatory = $true)]
    [string]$Password,

    [Parameter(Mandatory = $true)]
    [string]$RemotePath
)

$ErrorActionPreference = "Stop"

$hostName = $Server.Trim() -replace '^ftps?://', '' -replace '/+$', ''
$remote = "/" + ($RemotePath.Trim() -replace '\\', '/' -replace '^/+', '')
$uri = "ftp://$hostName$remote"

function Test-FileGone([string]$message) {
    return $message -match '550|not found|cannot find|does not exist'
}

$attempt = 0
$maxAttempts = 3

while ($attempt -lt $maxAttempts) {
    $attempt++
    try {
        $request = [System.Net.FtpWebRequest]::Create($uri)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
        $request.Credentials = New-Object System.Net.NetworkCredential($Username, $Password)
        $request.UseBinary = $true
        $request.UsePassive = $true
        $request.KeepAlive = $false
        $request.EnableSsl = $false
        $request.Timeout = 60000

        $response = $request.GetResponse()
        Write-Host "Removed $remote"
        $response.Close()
        exit 0
    }
    catch {
        $message = $_.Exception.Message
        if ($_.Exception.InnerException) {
            $message = "$message $($_.Exception.InnerException.Message)"
        }

        if (Test-FileGone $message) {
            Write-Host "Remote file already absent: $remote"
            exit 0
        }

        Write-Warning "Attempt $attempt/$maxAttempts failed to remove $remote : $message"
        if ($attempt -ge $maxAttempts) {
            throw
        }

        Start-Sleep -Seconds 5
    }
}
