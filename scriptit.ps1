### Get-ChildItem c:\encode | %{ write-host ("videoencoder.exe ", "`"", $_.FullName, "`" `"","\\jbod\encode\videoout\",  $_.name, "`"") -separator "" }



$bat1 = "c:\v\1.bat"
$bat2 = "c:\v2\2.bat"

remove-item $bat1
remove-item $bat2

$list = Get-ChildItem c:\encode

$i = 0;
foreach ($item in $list)
{
	$i++;
	
	#$foo = $i % 2

	[string]$command = "videoencoder.exe " + "`""+ $item.FullName+ "`" `""+"\\jbod\encode\videoout\"+  $item.name+ "`""

	if ( $i % 2 -eq 0 )
	{
		Add-content $bat1 $command
	}
	else
	{
		Add-content $bat2 $command
	}
}



