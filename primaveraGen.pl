#!/usr/bin/perl
use strict;
use warnings;


my @scripts = ("ServicesGenerator.sh","EditorsGenerator.sh");

my @data_array = ("Accounting","Internal","HumanResources","CashManagement","ContactsOpportunities");

my $string  = "";

foreach my $x (@data_array) {
	$string = $string .",".$x;
}

#foreach my $script (@scripts) {
#	foreach my $data (@data_array) { 
#		system("bash $script $data");
#	} 
#}

substr($string, 0, 1) = "";

#print "$string";

#system ("bash EventDelegatesGenerator.sh $string");

#system ("bash ExtensibilityEventsGenerator.sh $string");


#system ("bash ModuleConnectorsGenerator.sh $string");

#system ("bash ModuleCSProjGenerator.sh $string");
