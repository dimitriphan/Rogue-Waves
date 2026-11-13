"""The save fuzz now runs inside rules_check.py, which compiles the mode's rules files whole (this script used to copy
methods out of them one by one). Kept so the old command still works:

    cd TDS_Mod/ModSource && python3 ../Tools/cloud-check/save_fuzz.py [RogueWavesMode/Code]"""
import os, subprocess, sys
sys.exit(subprocess.run([sys.executable, os.path.join(os.path.dirname(os.path.abspath(__file__)), 'rules_check.py')] + sys.argv[1:]).returncode)
