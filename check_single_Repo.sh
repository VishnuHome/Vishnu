#!/bin/bash
# Script zum Überprüfen des aktuellen Verzeichnisses auf Aktualität
# und Synchronität mit allen remote-Repositories.
# 28.07.2020 Erik Nagel: erstellt.

die() { echo "$@" 1>&2 ; exit 1; }

# function to list a single remote status
checkRepoStatus() {
	actRemote=$1
	actBranch=$2
  #echo git fetch "$actRemote" "$actBranch"
  git fetch "$actRemote" "$actBranch" >/dev/null 2>&1
  echo git diff "${actRemote}/${actBranch}" "$actBranch:"
	git diff "${actRemote}/${actBranch}" "$actBranch"
	echo "-------------------------------------------------------------------"
}


# function for a single repo (locally)
workOnRepo() {
	orgDir="$(pwd)"
	cd $1 || die "Fehler beim Wechsel nach $1"
	# get the root directory in case you run script from deeper into the repo
	
	gitRoot="$(git rev-parse --show-toplevel)"

	cd "$gitRoot" || die "Fehler beim Wechsel nach $gitRoot"
	echo "---------------------------------------------------------------------------------------------------"
	pwd
	echo "---------------------------------------------------------------------------------------------------"
  git fetch --prune
	# echo git status
	
  git status

  trackedRemote=$(git status -sb|grep '##'|sed -e 's/## //'|sed -e 's/.*\.\.\.//'|sed -e 's/\/.*//')
  actBranch=$(git branch --show-current)
	for actRemote in $(git remote|grep -v "$trackedRemote"); do checkRepoStatus $actRemote $actBranch; done

  lastLogText=$(git log --name-status HEAD^..HEAD 2>/dev/null|grep "    "|sed "s/^ \+//")
  if [[ -z $lastLogText ]]
	then
	  lastLogText=$(git log --name-status HEAD|grep "    "|sed "s/^ \+//")
	fi

  lastLongLogDate=$(git log --name-status HEAD^..HEAD 2>/dev/null|grep "Date:"|sed "s/Date:\s*//"|sed "s/ +.*//"|head -1)
  if [[ -z $lastLongLogDate ]]
	then
	  lastLongLogDate=$(git log --name-status HEAD|grep "Date:"|sed "s/Date:\s*//"|sed "s/ +.*//"|head -1)
	fi

  lastLogDate=$(date -d"$lastLongLogDate" +%d.%m.%Y)

  lastLogAuthor=$(git log --name-status HEAD^..HEAD 2>/dev/null|grep "Author:"|sed "s/\s*<.*//"|sed "s/Author: //")
  if [[ -z $lastLogAuthor ]]
	then
	  lastLogAuthor=$(git log --name-status HEAD|grep "Author:"|sed "s/\s*<.*//"|sed "s/Author: //")
	fi
 
	echo "${lastLogDate} ${lastLogAuthor}: ${lastLogText}"
	cd "$orgDir" || die "Fehler beim Wechsel nach $orgDir"
}

# Main
workOnRepo "."
echo "---------------------------------------------------------------------------------------------------"
