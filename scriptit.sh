#!/bin/bash

SOURCE="C:\\encode"
DEST="\\\\jbod\encode\videoout"

for f in /cygdrive/c/encode/*; do
	#echo $f
	BASE_NAME=`basename "$f"`

	echo videoencoder "\"$SOURCE\\$BASE_NAME\"" "\"$DEST\\$BASE_NAME\""
done
