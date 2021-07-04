#!/usr/bin/perl -w

#my $srcname = $ARGV[0];
#my $tarname = $ARGV[1];
my $srcname = 'vs_mk';
my $tarname = 'vs_mk.inc';
print "'$srcname' -> '$tarname'\n";
my $filecontent = "";
open(F, "< $srcname") or die "Fehler beim Öffnen der Datei: $!\n";
foreach my $line (<F>) {$filecontent .= $line; }
close(F);
my $tfc = "_T(\"";
for ($i = 0; $i < length($filecontent); $i++) {
	my $c = ord(substr($filecontent, $i, 1));
	$c = 255 - $c;
	$tfc .= sprintf("\\x%02x", $c);
}
$tfc .= "\");";

open(F, "> $tarname");
print F $tfc;
close(F);
