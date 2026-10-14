import logging
from pathlib import Path
logging.basicConfig(level=logging.DEBUG)

import os
PROJECT_ROOT = Path(__file__).parent.resolve()
BIN = PROJECT_ROOT / "bin"

os.add_dll_directory(BIN)
os.chdir(BIN)

from harvesters.core import Harvester

h = Harvester()
h.add_file(os.path.join(BIN, "GenTL.dll"))
h.update()
print("Devices:", h.device_info_list)
h.reset()