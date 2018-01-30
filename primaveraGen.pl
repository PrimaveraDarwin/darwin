#!/usr/bin/perl
use strict;
use warnings;


my @scripts = ("ServicesGenerator","EditorsGenerator.sh");

my @data_array = ("Base", "Sales","Purchases","EquipmentsFixedAssets","Accounting","Internal","HumanResources","CashManagement","ContactsOpportunities");

my $string  = "";

foreach my $script (@scripts) {
	foreach my $data (@data_array) { 
    		$string = $string . "," . $data;
		system("bash $script $data");
	} 
}

substr($string, 0, 1) = "";

#print "$string";
system ("bash ExtensibilityEventsGenerator.sh $string");

