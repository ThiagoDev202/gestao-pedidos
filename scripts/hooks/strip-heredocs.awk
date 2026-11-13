inbody {
  if ($0 == delim) inbody = 0
  next
}
{
  if (match($0, /<<-?[ ]*["']?[A-Za-z_][A-Za-z_0-9]*["']?/)) {
    tag = substr($0, RSTART, RLENGTH)
    gsub(/<<-?[ ]*/, "", tag)
    gsub(/["']/, "", tag)
    delim = tag
    inbody = 1
  }
  print
}
