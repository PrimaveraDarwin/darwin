#!/usr/bin/perl
use strict;
use warnings;

my @scripts = ("ServicesGenerator.sh","EditorGenerator.sh");

my @data_array = ("Base", "Sales","Purchases","EquipmentsFixedAssets","Accounting","Internal","HumanResources","CashManagement","ContactsOpportunities");


foreach my $script (@scripts) {
	foreach my $data (@data_array) { 
    		system("bash $script $data");
	} 
}
