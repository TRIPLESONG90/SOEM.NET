# Third-Party Attributions

## SOEM (Simple Open EtherCAT Master)

The `native/soem/` directory contains a vendored copy of SOEM source code.

- **Source**: https://github.com/OpenEtherCATsociety/SOEM
- **Version**: 2.0.0
- **License**: Dual-licensed – GPLv3 and a commercial license
- **Copyright**: RT-Labs AB (previously Ethercat Solutions Devices)

The full license text is reproduced in `native/soem/LICENSE.md`.

SOEM is used here as a static library linked into the `soem_wrapper` shared
library. The wrapper and managed bindings in `src/Soem.Net/` are therefore
also subject to the GPLv3 terms unless you obtain a commercial SOEM license.

For commercial licensing, contact: info.soem@rt-labs.com
